﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Transactions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Til.Lombok;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Til.Unity.Lombok {

    [Generator]
    public sealed class BufferSerialization : IIncrementalGenerator {

        private static readonly string AttributeName = typeof(ILombokAttribute).FullName!;

        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var sources = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeName,
                IsCandidate,
                Transform
            );
            context.AddSources(
                sources
            );
        }

        private bool IsCandidate(
            SyntaxNode node,
            CancellationToken cancellationToken) => node is ClassDeclarationSyntax;

        private GeneratorResult Transform(
            GeneratorAttributeSyntaxContext context,
            CancellationToken cancellationToken) {
            ClassDeclarationSyntax contextTargetNode = (ClassDeclarationSyntax)context.TargetNode;

            SemanticModel semanticModel = context.SemanticModel;

            if (!contextTargetNode.TryValidateType(
                    out var @namespace,
                    out var diagnostic
                )) {
                return new GeneratorResult(
                    diagnostic
                );
            }

            List<CSharpSyntaxNode> fieldList = new List<CSharpSyntaxNode>();

            foreach (var member in contextTargetNode.Members) {
                switch (member) {
                    // 检查成员是否是字段或属性  
                    case FieldDeclarationSyntax fieldDeclaration: {
                        AttributeSyntax? tryGetSpecifiedAttribute = fieldDeclaration.AttributeLists.tryGetSpecifiedAttribute(
                            nameof(BufferFieldAttribute)
                        );
                        if (tryGetSpecifiedAttribute is not null) {
                            foreach (VariableDeclaratorSyntax variableDeclaratorSyntax in fieldDeclaration.Declaration.Variables) {
                                fieldList.Add(
                                    variableDeclaratorSyntax
                                );
                            }
                        }
                        break;
                    }
                    case PropertyDeclarationSyntax propertyDeclaration: {
                        AttributeSyntax? tryGetSpecifiedAttribute = propertyDeclaration.AttributeLists.tryGetSpecifiedAttribute(
                            nameof(BufferFieldAttribute)
                        );
                        if (tryGetSpecifiedAttribute is not null) {
                            fieldList.Add(
                                tryGetSpecifiedAttribute
                            );
                        }
                        break;
                    }
                }
            }

            if (fieldList.Count == 0) {
                return GeneratorResult.Empty;
            }

            bool isNotValueType = !((semanticModel.GetSymbolInfo(
                                            contextTargetNode
                                        )
                                        .Symbol as ITypeSymbol)?.IsValueType
                                    ?? false);

            #region readField

            MethodDeclarationSyntax readField = MethodDeclaration(
                    IdentifierName(
                        "void"
                    ),
                    "read"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "reader"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                "Unity.Netcode.FastBufferReader"
                            )
                        ),
                    Parameter(
                            Identifier(
                                "value"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.OutKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    isNotValueType
                        ? new StatementSyntax[] {
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    ParseTypeName(
                                        "bool"
                                    ),
                                    SeparatedList(
                                        new[] {
                                            VariableDeclarator(
                                                "isNull"
                                            )
                                        }
                                    )
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.ByteUnpacker.ReadValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "reader"
                                                    )
                                                ),
                                                Argument(
                                                        IdentifierName(
                                                            "isNull"
                                                        )
                                                    )
                                                    .WithRefKindKeyword(
                                                        Token(
                                                            SyntaxKind.OutKeyword
                                                        )
                                                    )
                                            }
                                        )
                                    )
                                )
                            ),
                            IfStatement(
                                IdentifierName(
                                    "isNull"
                                ),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(
                                                "value"
                                            ),
                                            IdentifierName(
                                                "null!"
                                            )
                                        )
                                    ),
                                    ReturnStatement(
                                    )
                                )
                            ),
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(
                                        "value"
                                    ),
                                    ObjectCreationExpression(
                                            ParseTypeName(
                                                contextTargetNode.Identifier.Text
                                            )
                                        )
                                        .WithArgumentList(
                                            ArgumentList()
                                        )
                                )
                            )
                        }
                        : Array.Empty<StatementSyntax>()
                )
                .AddBodyStatements(
                    fieldList.Select(
                            cSharpSyntaxNode => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(
                                                $"Unity.Netcode.NetworkVariableSerialization<{type}>"
                                            ),
                                            IdentifierName(
                                                "Read"
                                            )
                                        ),
                                        ArgumentList(
                                            SeparatedList(
                                                new List<ArgumentSyntax>() {
                                                    Argument(
                                                        IdentifierName(
                                                            "reader"
                                                        )
                                                    ),
                                                    Argument(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(
                                                                    "value"
                                                                ),
                                                                IdentifierName(
                                                                    cSharpSyntaxNode.ToString()
                                                                )
                                                            )
                                                        )
                                                        .WithRefKindKeyword(
                                                            Token(
                                                                SyntaxKind.RefKeyword
                                                            )
                                                        )
                                                }
                                            )
                                        )
                                    )
                                );
                            }
                        )
                        .ToArray()
                );

            #endregion

            #region readDeltaField

            MethodDeclarationSyntax readDeltaField = MethodDeclaration(
                    IdentifierName(
                        "void"
                    ),
                    "readDelta"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "reader"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                "Unity.Netcode.FastBufferReader"
                            )
                        ),
                    Parameter(
                            Identifier(
                                "value"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.RefKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    isNotValueType
                        ? new StatementSyntax[] {
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    ParseTypeName(
                                        "bool"
                                    ),
                                    SeparatedList(
                                        new[] {
                                            VariableDeclarator(
                                                "useRead"
                                            )
                                        }
                                    )
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.ByteUnpacker.ReadValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "reader"
                                                    )
                                                ),
                                                Argument(
                                                        IdentifierName(
                                                            "useRead"
                                                        )
                                                    )
                                                    .WithRefKindKeyword(
                                                        Token(
                                                            SyntaxKind.OutKeyword
                                                        )
                                                    )
                                            }
                                        )
                                    )
                                )
                            ),
                            IfStatement(
                                IdentifierName("useRead"),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(
                                                "read"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                            IdentifierName(
                                                                "reader"
                                                            )
                                                        ),
                                                        Argument(
                                                                IdentifierName(
                                                                    "value"
                                                                )
                                                            )
                                                            .WithRefKindKeyword(
                                                                Token(
                                                                    SyntaxKind.OutKeyword
                                                                )
                                                            ),
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    ReturnStatement()
                                )
                            ),
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    ParseTypeName(
                                        "bool"
                                    ),
                                    SeparatedList(
                                        new[] {
                                            VariableDeclarator(
                                                "isNull"
                                            )
                                        }
                                    )
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.ByteUnpacker.ReadValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "reader"
                                                    )
                                                ),
                                                Argument(
                                                        IdentifierName(
                                                            "isNull"
                                                        )
                                                    )
                                                    .WithRefKindKeyword(
                                                        Token(
                                                            SyntaxKind.OutKeyword
                                                        )
                                                    )
                                            }
                                        )
                                    )
                                )
                            ),
                            IfStatement(
                                IdentifierName(
                                    "isNull"
                                ),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(
                                                "value"
                                            ),
                                            IdentifierName(
                                                "null!"
                                            )
                                        )
                                    ),
                                    ReturnStatement(
                                    )
                                )
                            ),
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "value"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(
                                                "value"
                                            ),
                                            ObjectCreationExpression(
                                                    ParseTypeName(
                                                        contextTargetNode.Identifier.Text
                                                    )
                                                )
                                                .WithArgumentList(
                                                    ArgumentList()
                                                )
                                        )
                                    )
                                )
                            )
                        }
                        : Array.Empty<StatementSyntax>()
                )
                .AddBodyStatements(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(
                                Token(
                                    SyntaxKind.IntKeyword
                                )
                            ),
                            SeparatedList(
                                new[] {
                                    VariableDeclarator(
                                        Identifier(
                                            "tag"
                                        )
                                    )
                                }
                            )
                        )
                    ),
                    ExpressionStatement(
                        InvocationExpression(
                            IdentifierName(
                                "Unity.Netcode.ByteUnpacker.ReadValuePacked"
                            ),
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            IdentifierName(
                                                "reader"
                                            )
                                        ),
                                        Argument(
                                                IdentifierName(
                                                    "tag"
                                                )
                                            )
                                            .WithRefKindKeyword(
                                                Token(
                                                    SyntaxKind.OutKeyword
                                                )
                                            )
                                    }
                                )
                            )
                        )
                    )
                )
                .AddBodyStatements(
                    fieldList.Select(
                            (cSharpSyntaxNode, id) => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)IfStatement(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        ParenthesizedExpression(
                                            BinaryExpression(
                                                SyntaxKind.BitwiseAndExpression,
                                                IdentifierName("tag"),
                                                ParenthesizedExpression(
                                                    BinaryExpression(
                                                        SyntaxKind.LeftShiftExpression,
                                                        LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            Literal(1)
                                                        ),
                                                        IdentifierName(id.ToString())
                                                    )
                                                )
                                            )
                                        ),
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(0)
                                        )
                                    ),
                                    Block(
                                        ExpressionStatement(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(
                                                        $"Unity.Netcode.NetworkVariableSerialization<{type}>"
                                                    ),
                                                    IdentifierName(
                                                        "ReadDelta"
                                                    )
                                                ),
                                                ArgumentList(
                                                    SeparatedList(
                                                        new List<ArgumentSyntax>() {
                                                            Argument(
                                                                IdentifierName(
                                                                    "reader"
                                                                )
                                                            ),
                                                            Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "value"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                )
                                                                .WithRefKindKeyword(
                                                                    Token(
                                                                        SyntaxKind.RefKeyword
                                                                    )
                                                                )
                                                        }
                                                    )
                                                )
                                            )
                                        )
                                    )
                                );

                                /*return (StatementSyntax)ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(
                                                $"Unity.Netcode.NetworkVariableSerialization<{type}>"
                                            ),
                                            IdentifierName(
                                                "ReadDelta"
                                            )
                                        ),
                                        ArgumentList(
                                            SeparatedList(
                                                new List<ArgumentSyntax>() {
                                                    Argument(
                                                        IdentifierName(
                                                            "reader"
                                                        )
                                                    ),
                                                    Argument(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(
                                                                    "value"
                                                                ),
                                                                IdentifierName(
                                                                    cSharpSyntaxNode.ToString()
                                                                )
                                                            )
                                                        )
                                                        .WithRefKindKeyword(
                                                            Token(
                                                                SyntaxKind.RefKeyword
                                                            )
                                                        )
                                                }
                                            )
                                        )
                                    )
                                );*/
                            }
                        )
                        .ToArray()
                );

            #endregion

            #region writeField

            MethodDeclarationSyntax writeField = MethodDeclaration(
                    IdentifierName(
                        "void"
                    ),
                    "write"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "writer"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                "Unity.Netcode.FastBufferWriter"
                            )
                        ),
                    Parameter(
                            Identifier(
                                "value"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.InKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    isNotValueType
                        ? new StatementSyntax[] {
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "value"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(
                                                "Unity.Netcode.BytePacker.WriteValuePacked"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                            IdentifierName(
                                                                "writer"
                                                            )
                                                        ),
                                                        Argument(
                                                            IdentifierName(
                                                                "true"
                                                            )
                                                        ),
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    ReturnStatement()
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.BytePacker.WriteValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "writer"
                                                    )
                                                ),
                                                Argument(
                                                    IdentifierName(
                                                        "false"
                                                    )
                                                ),
                                            }
                                        )
                                    )
                                )
                            )
                        }
                        : Array.Empty<StatementSyntax>()
                )
                .AddBodyStatements(
                )
                .AddBodyStatements(
                    fieldList.Select(
                            cSharpSyntaxNode => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)ExpressionStatement(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(
                                                    $"Unity.Netcode.NetworkVariableSerialization<{type}>"
                                                ),
                                                IdentifierName(
                                                    "Write"
                                                )
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new List<ArgumentSyntax>() {
                                                        Argument(
                                                            IdentifierName(
                                                                "writer"
                                                            )
                                                        ),
                                                        Argument(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName(
                                                                        "value"
                                                                    ),
                                                                    IdentifierName(
                                                                        cSharpSyntaxNode.ToString()
                                                                    )
                                                                )
                                                            )
                                                            .WithRefKindKeyword(
                                                                Token(
                                                                    SyntaxKind.RefKeyword
                                                                )
                                                            )
                                                    }
                                                )
                                            )
                                        )
                                    )
                                    ;
                            }
                        )
                        .ToArray()
                );

            #endregion

            #region writeDeltaField

            MethodDeclarationSyntax writeDeltaField = MethodDeclaration(
                    IdentifierName(
                        "void"
                    ),
                    "writeDelta"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "writer"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                "Unity.Netcode.FastBufferWriter"
                            )
                        ),
                    Parameter(
                            Identifier(
                                "value"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.InKeyword
                                )
                            )
                        ),
                    Parameter(
                            Identifier(
                                "previousValue"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.InKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    isNotValueType
                        ? new StatementSyntax[] {
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "previousValue"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(
                                                "Unity.Netcode.BytePacker.WriteValuePacked"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                            IdentifierName(
                                                                "writer"
                                                            )
                                                        ),
                                                        Argument(
                                                            IdentifierName(
                                                                "true"
                                                            )
                                                        ),
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(
                                                "write"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                            IdentifierName(
                                                                "writer"
                                                            )
                                                        ),
                                                        Argument(
                                                                IdentifierName(
                                                                    "value"
                                                                )
                                                            )
                                                            .WithRefKindKeyword(
                                                                Token(
                                                                    SyntaxKind.InKeyword
                                                                )
                                                            ),
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    ReturnStatement()
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.BytePacker.WriteValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "writer"
                                                    )
                                                ),
                                                Argument(
                                                    IdentifierName(
                                                        "false"
                                                    )
                                                ),
                                            }
                                        )
                                    )
                                )
                            ),
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "value"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        InvocationExpression(
                                            IdentifierName(
                                                "Unity.Netcode.BytePacker.WriteValuePacked"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                            IdentifierName(
                                                                "writer"
                                                            )
                                                        ),
                                                        Argument(
                                                            IdentifierName(
                                                                "true"
                                                            )
                                                        ),
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    ReturnStatement()
                                )
                            ),
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName(
                                        "Unity.Netcode.BytePacker.WriteValuePacked"
                                    ),
                                    ArgumentList(
                                        SeparatedList(
                                            new[] {
                                                Argument(
                                                    IdentifierName(
                                                        "writer"
                                                    )
                                                ),
                                                Argument(
                                                    IdentifierName(
                                                        "false"
                                                    )
                                                ),
                                            }
                                        )
                                    )
                                )
                            )
                        }
                        : Array.Empty<StatementSyntax>()
                )
                .AddBodyStatements(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PredefinedType(
                                Token(
                                    SyntaxKind.IntKeyword
                                )
                            ),
                            SeparatedList(
                                new[] {
                                    VariableDeclarator(
                                        Identifier(
                                            "tag"
                                        ),
                                        null,
                                        EqualsValueClause(
                                            LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(
                                                    0
                                                )
                                            )
                                        )
                                    )
                                }
                            )
                        )
                    )
                )
                .AddBodyStatements(
                    fieldList.Select(
                            (cSharpSyntaxNode, i) => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)IfStatement(
                                    PrefixUnaryExpression(
                                        SyntaxKind.LogicalNotExpression,
                                        InvocationExpression(
                                            IdentifierName(
                                                $"Unity.Netcode.NetworkVariableSerialization<{type}>.AreEqual"
                                            ),
                                            ArgumentList(
                                                SeparatedList(
                                                    new[] {
                                                        Argument(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName(
                                                                        "previousValue"
                                                                    ),
                                                                    IdentifierName(
                                                                        cSharpSyntaxNode.ToString()
                                                                    )
                                                                )
                                                            )
                                                            .WithRefKindKeyword(
                                                                Token(
                                                                    SyntaxKind.RefKeyword
                                                                )
                                                            ),
                                                        Argument(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName(
                                                                        "value"
                                                                    ),
                                                                    IdentifierName(
                                                                        cSharpSyntaxNode.ToString()
                                                                    )
                                                                )
                                                            )
                                                            .WithRefKindKeyword(
                                                                Token(
                                                                    SyntaxKind.RefKeyword
                                                                )
                                                            )
                                                    }
                                                )
                                            )
                                        )
                                    ),
                                    Block(
                                        ExpressionStatement(
                                            AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName("tag"),
                                                BinaryExpression(
                                                    SyntaxKind.BitwiseOrExpression,
                                                    IdentifierName("tag"),
                                                    ParenthesizedExpression(
                                                        BinaryExpression(
                                                            SyntaxKind.LeftShiftExpression,
                                                            LiteralExpression(
                                                                SyntaxKind.NumericLiteralExpression,
                                                                Literal(1)
                                                            ),
                                                            IdentifierName(i.ToString())
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                );
                            }
                        )
                        .ToArray()
                )
                .AddBodyStatements(
                    ExpressionStatement(
                        InvocationExpression(
                            IdentifierName(
                                "Unity.Netcode.BytePacker.WriteValuePacked"
                            ),
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            IdentifierName(
                                                "writer"
                                            )
                                        ),
                                        Argument(
                                            IdentifierName(
                                                "tag"
                                            )
                                        ),
                                    }
                                )
                            )
                        )
                    )
                )
                .AddBodyStatements(
                    fieldList.Select(
                            (cSharpSyntaxNode, id) => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)IfStatement(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        ParenthesizedExpression(
                                            BinaryExpression(
                                                SyntaxKind.BitwiseAndExpression,
                                                IdentifierName("tag"),
                                                ParenthesizedExpression(
                                                    BinaryExpression(
                                                        SyntaxKind.LeftShiftExpression,
                                                        LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            Literal(1)
                                                        ),
                                                        IdentifierName(id.ToString())
                                                    )
                                                )
                                            )
                                        ),
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(0)
                                        )
                                    ),
                                    Block(
                                        ExpressionStatement(
                                            InvocationExpression(
                                                IdentifierName(
                                                    $"Unity.Netcode.NetworkVariableSerialization<{type}>.WriteDelta"
                                                ),
                                                ArgumentList(
                                                    SeparatedList(
                                                        new List<ArgumentSyntax>() {
                                                            Argument(
                                                                IdentifierName(
                                                                    "writer"
                                                                )
                                                            ),
                                                            Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "value"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                )
                                                                .WithRefKindKeyword(
                                                                    Token(
                                                                        SyntaxKind.RefKeyword
                                                                    )
                                                                ),
                                                            Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "previousValue"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                )
                                                                .WithRefKindKeyword(
                                                                    Token(
                                                                        SyntaxKind.RefKeyword
                                                                    )
                                                                )
                                                        }
                                                    )
                                                )
                                            )
                                        )
                                    )
                                );
                            }
                        )
                        .ToArray()
                );

            #endregion

            #region duplicateValue

            MethodDeclarationSyntax duplicateValue = MethodDeclaration(
                    IdentifierName(
                        "void"
                    ),
                    "duplicateValue"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "value"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.InKeyword
                                )
                            )
                        ),
                    Parameter(
                            Identifier(
                                "duplicatedValue"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.RefKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    isNotValueType
                        ? new StatementSyntax[] {
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "value"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(
                                                "duplicatedValue"
                                            ),
                                            IdentifierName(
                                                "null!"
                                            )
                                        )
                                    ),
                                    ReturnStatement()
                                )
                            ),
                            IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(
                                        "duplicatedValue"
                                    ),
                                    LiteralExpression(
                                        SyntaxKind.NullLiteralExpression
                                    )
                                ),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(
                                                "duplicatedValue"
                                            ),
                                            ObjectCreationExpression(
                                                    ParseTypeName(
                                                        contextTargetNode.Identifier.Text
                                                    )
                                                )
                                                .WithArgumentList(
                                                    ArgumentList()
                                                )
                                        )
                                    )
                                )
                            )
                        }
                        : Array.Empty<StatementSyntax>()
                )
                .AddBodyStatements(
                    fieldList.Select(
                            cSharpSyntaxNode => {
                                string type = cSharpSyntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax
                                    ? ((VariableDeclarationSyntax)variableDeclaratorSyntax.Parent!).Type.ToString()
                                    : ((PropertyDeclarationSyntax)cSharpSyntaxNode).Type.ToString();

                                return (StatementSyntax)IfStatement(
                                        PrefixUnaryExpression(
                                            SyntaxKind.LogicalNotExpression,
                                            InvocationExpression(
                                                IdentifierName(
                                                    $"Unity.Netcode.NetworkVariableSerialization<{type}>.AreEqual"
                                                ),
                                                ArgumentList(
                                                    SeparatedList(
                                                        new[] {
                                                            Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "duplicatedValue"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                )
                                                                .WithRefKindKeyword(
                                                                    Token(
                                                                        SyntaxKind.RefKeyword
                                                                    )
                                                                ),
                                                            Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "value"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                )
                                                                .WithRefKindKeyword(
                                                                    Token(
                                                                        SyntaxKind.RefKeyword
                                                                    )
                                                                )
                                                        }
                                                    )
                                                )
                                            )
                                        ),
                                        Block(
                                            ExpressionStatement(
                                                InvocationExpression(
                                                    IdentifierName(
                                                        $"Unity.Netcode.NetworkVariableSerialization<{type}>.Duplicate"
                                                    ),
                                                    ArgumentList(
                                                        SeparatedList(
                                                            new List<ArgumentSyntax>() {
                                                                Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(
                                                                            "value"
                                                                        ),
                                                                        IdentifierName(
                                                                            cSharpSyntaxNode.ToString()
                                                                        )
                                                                    )
                                                                ),
                                                                Argument(
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(
                                                                                "duplicatedValue"
                                                                            ),
                                                                            IdentifierName(
                                                                                cSharpSyntaxNode.ToString()
                                                                            )
                                                                        )
                                                                    )
                                                                    .WithRefKindKeyword(
                                                                        Token(
                                                                            SyntaxKind.RefKeyword
                                                                        )
                                                                    )
                                                            }
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                    ;
                            }
                        )
                        .ToArray()
                );

            #endregion

            #region equals

            MethodDeclarationSyntax equals = MethodDeclaration(
                    IdentifierName(
                        "bool"
                    ),
                    "equals"
                )
                .AddModifiers(
                    Token(
                        SyntaxKind.PublicKeyword
                    ),
                    Token(
                        SyntaxKind.StaticKeyword
                    )
                )
                .AddParameterListParameters(
                    Parameter(
                            Identifier(
                                "a"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.RefKeyword
                                )
                            )
                        ),
                    Parameter(
                            Identifier(
                                "b"
                            )
                        )
                        .WithType(
                            IdentifierName(
                                contextTargetNode.Identifier.Text
                            )
                        )
                        .WithModifiers(
                            TokenList(
                                Token(
                                    SyntaxKind.RefKeyword
                                )
                            )
                        )
                )
                .AddBodyStatements(
                    ReturnStatement(
                        InvocationExpression(
                            IdentifierName("object.Equals"),
                            ArgumentList(
                                SeparatedList(
                                    new[] {
                                        Argument(
                                            IdentifierName(
                                                "a"
                                            )
                                        ),
                                        Argument(
                                            IdentifierName(
                                                "b"
                                            )
                                        )

                                    }
                                )
                            )
                        )
                    )
                );

            #endregion

            CompilationUnitSyntax compilationUnitSyntax = CompilationUnit()
                .WithUsings(
                    contextTargetNode.GetUsings()
                )
                .AddMembers(
                    NamespaceDeclaration(
                            @namespace
                        )
                        .AddMembers(
                            contextTargetNode
                                .CreateNewPartialClass()
                                .AddMembers(
                                    readField,
                                    readDeltaField,
                                    writeField,
                                    writeDeltaField,
                                    duplicateValue,
                                    equals
                                )
                                .AddMembers(
                                    MethodDeclaration(
                                            IdentifierName(
                                                "void"
                                            ),
                                            contextTargetNode.Identifier.Text + "InitializeOnLoad"
                                        )
                                        .AddModifiers(
                                            Token(
                                                SyntaxKind.ProtectedKeyword
                                            ),
                                            Token(
                                                SyntaxKind.StaticKeyword
                                            )
                                        )
                                        .AddAttributeLists(
                                            AttributeList(
                                                SeparatedList(
                                                    new[] {
                                                        Attribute(
                                                            ParseName("UnityEngine.RuntimeInitializeOnLoadMethodAttribute"),
                                                            AttributeArgumentList(
                                                                SeparatedList(
                                                                    new[] {
                                                                        AttributeArgument(
                                                                            ParseTypeName("UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded")
                                                                        )
                                                                    }
                                                                )
                                                            )
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                        .AddAttributeLists(
                                            AttributeList(
                                                SeparatedList(
                                                    new[] {
                                                        Attribute(
                                                            ParseName(
                                                                "UnityEditor.InitializeOnLoadMethodAttribute"
                                                            )
                                                        )
                                                    }
                                                )
                                            )
                                        )
                                        .AddBodyStatements(
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName(
                                                            $"Unity.Netcode.UserNetworkVariableSerialization<{contextTargetNode.Identifier.Text}>"
                                                        ),
                                                        IdentifierName(
                                                            "ReadValue"
                                                        )
                                                    ),
                                                    IdentifierName(
                                                        "read"
                                                    )
                                                )
                                            ),
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName(
                                                            $"Unity.Netcode.UserNetworkVariableSerialization<{contextTargetNode.Identifier.Text}>"
                                                        ),
                                                        IdentifierName(
                                                            "WriteValue"
                                                        )
                                                    ),
                                                    IdentifierName(
                                                        "write"
                                                    )
                                                )
                                            ),
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName(
                                                            $"Unity.Netcode.UserNetworkVariableSerialization<{contextTargetNode.Identifier.Text}>"
                                                        ),
                                                        IdentifierName(
                                                            "WriteDelta"
                                                        )
                                                    ),
                                                    IdentifierName(
                                                        "writeDelta"
                                                    )
                                                )
                                            ),
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName(
                                                            $"Unity.Netcode.UserNetworkVariableSerialization<{contextTargetNode.Identifier.Text}>"
                                                        ),
                                                        IdentifierName(
                                                            "ReadDelta"
                                                        )
                                                    ),
                                                    IdentifierName(
                                                        "readDelta"
                                                    )
                                                )
                                            ),
                                            ExpressionStatement(
                                                AssignmentExpression(
                                                    SyntaxKind.SimpleAssignmentExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName(
                                                            $"Unity.Netcode.UserNetworkVariableSerialization<{contextTargetNode.Identifier.Text}>"
                                                        ),
                                                        IdentifierName(
                                                            "DuplicateValue"
                                                        )
                                                    ),
                                                    IdentifierName(
                                                        "duplicateValue"
                                                    )
                                                )
                                            ),
                                            ExpressionStatement(
                                                InvocationExpression(
                                                    IdentifierName(
                                                        $"typeof(Unity.Netcode.NetworkVariableSerialization<{contextTargetNode.Identifier.Text}>).GetProperty(\"AreEqual\", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue"
                                                    ),
                                                    ArgumentList(
                                                        SeparatedList(
                                                            new[] {
                                                                Argument(IdentifierName("null")),
                                                                Argument(IdentifierName($"new Unity.Netcode.NetworkVariableSerialization<{contextTargetNode.Identifier.Text}>.EqualsDelegate(equals)"))
                                                            }
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                )
                        )
                );

            string fullString = compilationUnitSyntax
                .NormalizeWhitespace()
                .ToFullString();

            return new GeneratorResult(
                contextTargetNode.GetHintName(
                    @namespace
                ),
                SourceText.From(
                    fullString.Replace("[UnityEditor.InitializeOnLoadMethodAttribute]", "\n#if UNITY_EDITOR\n        [UnityEditor.InitializeOnLoadMethodAttribute]\n#endif\n"),
                    Encoding.UTF8
                )
            );
        }

    }

}
