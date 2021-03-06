﻿using System;
using System.Collections.Generic;
using System.IO;
using MonC;
using MonC.Semantics;

namespace Driver
{
    public interface IInput
    {
        public void WriteInputChain(TextWriter writer);
    }

    public interface ILexInput : IInput { }

    public interface IParseInput : IInput
    {
        public List<Token> GetTokens();
        public FileInfo GetFileInfo();
    }

    public interface ICodeGenInput : IInput
    {
        public void RunHeaderPass();
        public void RunAnalyserPass();
        public SemanticModule GetSemanticModule();
        public FileInfo GetFileInfo();
    }

    public interface IBackendInput : IInput
    {
        public void RunHeaderPass();
        public void RunAnalyserPass();
        public IModuleArtifact GetModuleArtifact();
    }

    public interface ILinkInput : IInput
    {
        public List<IModuleArtifact> GetModuleArtifacts();
    }

    public interface IVMInput : IInput
    {
        public IVMModuleArtifact GetVMModuleArtifact();
    }

    public interface IJobAction
    {
        public static IJobAction FromPhase(Phase phase)
        {
            switch (phase) {
                case Phase.Lex:
                    return new LexJobAction();
                case Phase.Parse:
                    return new ParseJobAction();
                case Phase.CodeGen:
                    return new CodeGenJobAction();
                case Phase.Backend:
                    return new BackendJobAction();
                case Phase.Link:
                    return new LinkJobAction();
                case Phase.VM:
                    return new VMJobAction();
            }

            throw new InvalidOperationException($"Unsupported phase {phase} for JobAction");
        }

        public ITool Accept(ToolChain toolChain, Job job, IInput input);
    }

    public sealed class LexJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildLexJobTool(job, (ILexInput) input);
    }

    public sealed class ParseJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildParseJobTool(job, (IParseInput) input);
    }

    public sealed class CodeGenJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildCodeGenJobTool(job, (ICodeGenInput) input);
    }

    public sealed class BackendJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildBackendJobTool(job, (IBackendInput) input);
    }

    public sealed class LinkJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildLinkJobTool(job, (ILinkInput) input);
    }

    public sealed class VMJobAction : IJobAction
    {
        public ITool Accept(ToolChain toolChain, Job job, IInput input) =>
            toolChain.BuildVMJobTool(job, (IVMInput) input);
    }
}
