using Esprima.Ast;
using Esprima.Utils;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    // revised AST visitor as the one provided by Esprima.NET is quite lacking currently:
    // https://github.com/sebastienros/esprima-dotnet/pull/148
    // https://github.com/sebastienros/esprima-dotnet/pull/176
    internal class ImprovedAstVisitor : AstVisitor
    {
        protected override void VisitArrayExpression(ArrayExpression arrayExpression)
        {
            for (int i = 0, n = arrayExpression.Elements.Count; i < n; i++)
            {
                Expression element = arrayExpression.Elements[i];
                if (element != null)
                    Visit(element);
            }
        }

        protected override void VisitArrayPattern(ArrayPattern arrayPattern)
        {
            for (int i = 0, n = arrayPattern.Elements.Count; i < n; i++)
            {
                Expression element = arrayPattern.Elements[i];
                if (element != null)
                    Visit(element);
            }
        }

        protected override void VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
        {
            VisitFunctionExpression(arrowFunctionExpression);
        }

        protected override void VisitArrowParameterPlaceHolder(ArrowParameterPlaceHolder arrowParameterPlaceHolder)
        {
            // ArrowParameterPlaceHolder nodes never appear in the final tree and only used during the construction of a tree.
        }

        protected override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            Visit(assignmentExpression.Left);
            Visit(assignmentExpression.Right);
        }

        protected override void VisitAssignmentPattern(AssignmentPattern assignmentPattern)
        {
            Visit(assignmentPattern.Left);
            Visit(assignmentPattern.Right);
        }

        protected override void VisitAwaitExpression(AwaitExpression awaitExpression)
        {
            Visit(awaitExpression.Argument);
        }

        protected override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            Visit(binaryExpression.Left);
            Visit(binaryExpression.Right);
        }

        protected override void VisitBlockStatement(BlockStatement blockStatement)
        {
            for (int i = 0, n = blockStatement.Body.Count; i < n; i++)
                Visit(blockStatement.Body[i]);
        }

        protected override void VisitBreakStatement(BreakStatement breakStatement)
        {
            if (breakStatement.Label != null)
                Visit(breakStatement.Label);
        }

        protected override void VisitCallExpression(CallExpression callExpression)
        {
            Visit(callExpression.Callee);

            for (int i = 0, n = callExpression.Arguments.Count; i < n; i++)
                Visit(callExpression.Arguments[i]);
        }

        protected override void VisitCatchClause(CatchClause catchClause)
        {
            if (catchClause.Param != null)
                Visit(catchClause.Param);

            Visit(catchClause.Body);
        }

        protected override void VisitChainExpression(ChainExpression chainExpression)
        {
            Visit(chainExpression.Expression);
        }

        protected override void VisitClassBody(ClassBody classBody)
        {
            for (int i = 0, n = classBody.Body.Count; i < n; i++)
                Visit(classBody.Body[i]);
        }

        protected override void VisitClassDeclaration(ClassDeclaration classDeclaration)
        {
            if (classDeclaration.Id != null)
                Visit(classDeclaration.Id);

            if (classDeclaration.SuperClass != null)
                Visit(classDeclaration.SuperClass);

            Visit(classDeclaration.Body);
        }

        protected override void VisitClassExpression(ClassExpression classExpression)
        {
            if (classExpression.Id != null)
                Visit(classExpression.Id);

            if (classExpression.SuperClass != null)
                Visit(classExpression.SuperClass);

            Visit(classExpression.Body);
        }

        protected override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            Visit(conditionalExpression.Test);
            Visit(conditionalExpression.Consequent);
            Visit(conditionalExpression.Alternate);
        }

        protected override void VisitContinueStatement(ContinueStatement continueStatement)
        {
            if (continueStatement.Label != null)
                Visit(continueStatement.Label);
        }

        protected override void VisitDebuggerStatement(DebuggerStatement debuggerStatement) { }

        protected override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            Visit(doWhileStatement.Body);
            Visit(doWhileStatement.Test);
        }

        protected override void VisitEmptyStatement(EmptyStatement emptyStatement) { }

        protected override void VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
        {
            Visit(exportAllDeclaration.Source);
        }

        protected override void VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
        {
            Visit(exportDefaultDeclaration.Declaration);
        }

        protected override void VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
        {
            if (exportNamedDeclaration.Declaration == null)
            {
                for (int i = 0, n = exportNamedDeclaration.Specifiers.Count; i < n; i++)
                    Visit(exportNamedDeclaration.Specifiers[i]);

                if (exportNamedDeclaration.Source != null)
                    Visit(exportNamedDeclaration.Source);
            }
            else
                Visit(exportNamedDeclaration.Declaration);
        }

        protected override void VisitExportSpecifier(ExportSpecifier exportSpecifier)
        {
            Visit(exportSpecifier.Local);
            Visit(exportSpecifier.Exported);
        }

        protected override void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            Visit(expressionStatement.Expression);
        }

        protected override void VisitForInStatement(ForInStatement forInStatement)
        {
            Visit(forInStatement.Left);
            Visit(forInStatement.Right);
            Visit(forInStatement.Body);
        }

        protected override void VisitForOfStatement(ForOfStatement forOfStatement)
        {
            Visit(forOfStatement.Left);
            Visit(forOfStatement.Right);
            Visit(forOfStatement.Body);
        }

        protected override void VisitForStatement(ForStatement forStatement)
        {
            if (forStatement.Init != null)
                Visit(forStatement.Init);

            if (forStatement.Test != null)
                Visit(forStatement.Test);

            if (forStatement.Update != null)
                Visit(forStatement.Update);

            Visit(forStatement.Body);
        }

        protected override void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            VisitFunctionExpression(functionDeclaration);
        }

        protected override void VisitFunctionExpression(IFunction function)
        {
            if (function.Id != null)
                Visit(function.Id);

            for (int i = 0, n = function.Params.Count; i < n; i++)
                Visit(function.Params[i]);

            Visit(function.Body);
        }

        protected override void VisitIdentifier(Identifier identifier) { }

        protected override void VisitIfStatement(IfStatement ifStatement)
        {
            Visit(ifStatement.Test);

            Visit(ifStatement.Consequent);

            if (ifStatement.Alternate != null)
                Visit(ifStatement.Alternate);
        }

        protected override void VisitImport(Import import) { }

        protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
        {
            for (int i = 0, n = importDeclaration.Specifiers.Count; i < n; i++)
                Visit(importDeclaration.Specifiers[i]);

            Visit(importDeclaration.Source);
        }

        protected override void VisitImportDefaultSpecifier(ImportDefaultSpecifier importDefaultSpecifier)
        {
            Visit(importDefaultSpecifier.Local);
        }

        protected override void VisitImportNamespaceSpecifier(ImportNamespaceSpecifier importNamespaceSpecifier)
        {
            Visit(importNamespaceSpecifier.Local);
        }

        protected override void VisitImportSpecifier(ImportSpecifier importSpecifier)
        {
            Visit(importSpecifier.Imported);
            Visit(importSpecifier.Local);
        }

        protected override void VisitLabeledStatement(LabeledStatement labeledStatement)
        {
            Visit(labeledStatement.Label);
            Visit(labeledStatement.Body);
        }

        protected override void VisitLiteral(Literal literal) { }

        protected override void VisitLogicalExpression(BinaryExpression binaryExpression)
        {
            VisitBinaryExpression(binaryExpression);
        }

        protected override void VisitMemberExpression(MemberExpression memberExpression)
        {
            Visit(memberExpression.Object);
            Visit(memberExpression.Property);
        }

        protected override void VisitMetaProperty(MetaProperty metaProperty)
        {
            Visit(metaProperty.Meta);
            Visit(metaProperty.Property);
        }

        protected override void VisitMethodDefinition(MethodDefinition methodDefinition)
        {
            Visit(methodDefinition.Key);
            Visit(methodDefinition.Value);
        }

        protected override void VisitNewExpression(NewExpression newExpression)
        {
            Visit(newExpression.Callee);

            for (int i = 0, n = newExpression.Arguments.Count; i < n; i++)
                Visit(newExpression.Arguments[i]);
        }

        protected override void VisitObjectExpression(ObjectExpression objectExpression)
        {
            for (int i = 0, n = objectExpression.Properties.Count; i < n; i++)
                Visit(objectExpression.Properties[i]);
        }

        protected override void VisitObjectPattern(ObjectPattern objectPattern)
        {
            for (int i = 0, n = objectPattern.Properties.Count; i < n; i++)
                Visit(objectPattern.Properties[i]);
        }

        protected override void VisitProgram(Program program)
        {
            for (int i = 0, n = program.Body.Count; i < n; i++)
                Visit(program.Body[i]);
        }

        protected override void VisitProperty(Property property)
        {
            Visit(property.Key);
            Visit(property.Value);
        }

        protected override void VisitRestElement(RestElement restElement)
        {
            Visit(restElement.Argument);
        }

        protected override void VisitReturnStatement(ReturnStatement returnStatement)
        {
            if (returnStatement.Argument != null)
                Visit(returnStatement.Argument);
        }

        protected override void VisitSequenceExpression(SequenceExpression sequenceExpression)
        {
            for (int i = 0, n = sequenceExpression.Expressions.Count; i < n; i++)
                Visit(sequenceExpression.Expressions[i]);
        }

        protected override void VisitSpreadElement(SpreadElement spreadElement)
        {
            Visit(spreadElement.Argument);
        }

        protected override void VisitSuper(Super super) { }

        protected override void VisitSwitchCase(SwitchCase switchCase)
        {
            if (switchCase.Test != null)
                Visit(switchCase.Test);

            for (int i = 0, n = switchCase.Consequent.Count; i < n; i++)
                Visit(switchCase.Consequent[i]);
        }

        protected override void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            Visit(switchStatement.Discriminant);

            for (int i = 0, n = switchStatement.Cases.Count; i < n; i++)
                Visit(switchStatement.Cases[i]);
        }

        protected override void VisitTaggedTemplateExpression(TaggedTemplateExpression taggedTemplateExpression)
        {
            Visit(taggedTemplateExpression.Tag);
            Visit(taggedTemplateExpression.Quasi);
        }

        protected override void VisitTemplateElement(TemplateElement templateElement) { }

        protected override void VisitTemplateLiteral(TemplateLiteral templateLiteral)
        {
            var n = templateLiteral.Expressions.Count;

            for (var i = 0; i < n; i++)
            {
                Visit(templateLiteral.Quasis[i]);
                Visit(templateLiteral.Expressions[i]);
            }

            Visit(templateLiteral.Quasis[n]);
        }

        protected override void VisitThisExpression(ThisExpression thisExpression) { }

        protected override void VisitThrowStatement(ThrowStatement throwStatement)
        {
            Visit(throwStatement.Argument);
        }

        protected override void VisitTryStatement(TryStatement tryStatement)
        {
            Visit(tryStatement.Block);

            if (tryStatement.Handler != null)
                Visit(tryStatement.Handler);

            if (tryStatement.Finalizer != null)
                Visit(tryStatement.Finalizer);
        }

        protected override void VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            Visit(unaryExpression.Argument);
        }

        protected override void VisitUpdateExpression(UpdateExpression updateExpression)
        {
            VisitUnaryExpression(updateExpression);
        }

        protected override void VisitVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            for (int i = 0, n = variableDeclaration.Declarations.Count; i < n; i++)
                Visit(variableDeclaration.Declarations[i]);
        }

        protected override void VisitVariableDeclarator(VariableDeclarator variableDeclarator)
        {
            Visit(variableDeclarator.Id);

            if (variableDeclarator.Init != null)
                Visit(variableDeclarator.Init);
        }

        protected override void VisitWhileStatement(WhileStatement whileStatement)
        {
            Visit(whileStatement.Test);
            Visit(whileStatement.Body);
        }

        protected override void VisitWithStatement(WithStatement withStatement)
        {
            Visit(withStatement.Object);
            Visit(withStatement.Body);
        }

        protected override void VisitYieldExpression(YieldExpression yieldExpression)
        {
            if (yieldExpression.Argument != null)
                Visit(yieldExpression.Argument);
        }
    }
}
