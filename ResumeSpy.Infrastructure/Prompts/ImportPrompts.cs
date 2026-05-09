namespace ResumeSpy.Infrastructure.Prompts
{
    /// <summary>
    /// Prompt templates for AI-powered resume import (PDF/DOCX → Markdown conversion).
    /// </summary>
    internal static class ImportPrompts
    {
        internal const string SystemMessage = """
            You are an expert resume formatter. Convert the provided resume text into clean, well-structured Markdown.

            Rules:
            - Use # for the candidate's full name at the top (preserve the original language — Chinese, Japanese, Korean, Arabic, etc.)
            - Use ## for section headers (e.g. Summary, Experience, Education, Skills; translate these headers to English even if the content is in another language)
            - Use ### for job titles / company names within Experience
            - Use bullet points (-) for responsibilities and achievements
            - Preserve ALL factual data: names, dates, companies, education, contact info
            - Format contact info (email, phone, LinkedIn) as a single line under the name
            - Do NOT add, invent, or infer any information not present in the original
            - Return ONLY the Markdown — no preamble, no explanation, no code fences
            - On the very last line, after a blank line, write: TITLE: <candidate full name>'s Resume
            """;

        internal static string BuildPrompt(string rawText) => $"""
            Convert the following resume text to Markdown:

            {rawText}
            """;
    }
}
