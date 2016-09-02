﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.V102;
using System.Xml;
using System.Xml.Linq;

namespace FhirProfilePublisher.Engine
{
    public class HtmlGenerator
    {
        public HtmlGenerator()
        {
        }

        public string Generate(string[] inputFilePaths, string outputDirectory, TextContent textContent, bool showEverythingOnOnePage, bool showResourcesInW5Group, ResourceMaturity[] listOnlyResourcesWithMaturity)
        {
            if (inputFilePaths == null)
                throw new ArgumentNullException("inputFilePaths");

            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentNullException("outputDirectory");

            OutputPaths outputPaths = new OutputPaths
            (
                outputDirectory: Path.Combine(outputDirectory, "Generated"),
                stylesRelativePath: "styles",
                imagesRelativePath: "images",
                scriptsRelativePath: "scripts",
                structureDefinitionPath: "StructureDefinition", 
                valueSetPath: "ValueSet"
            );

            Pages.Instance.PageHeader = textContent.HeaderText;
            Pages.Instance.PageTitleSuffix = textContent.PageTitleSuffix;
            Pages.Instance.TemplatePage = textContent.PageTemplate;

            ResourceFileSet resourceFileSet = new ResourceFileSet();
            resourceFileSet.LoadXmlResourceFiles(inputFilePaths.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray());

            return GenerateHtml(resourceFileSet, outputPaths, textContent, showEverythingOnOnePage, showResourcesInW5Group, listOnlyResourcesWithMaturity);
        }

        private string GenerateHtml(ResourceFileSet resourceFileSet, OutputPaths outputPaths, TextContent textContent, bool showEverythingOnOnePage, bool showResourceInW5Group, ResourceMaturity[] listOnlyResourcesWithMaturity)
        {
            // copy supporting files
            Styles.WriteStylesToDisk(outputPaths);
            Images.WriteImagesToDisk(outputPaths);
            Scripts.WriteScriptsToDisk(outputPaths);

            // structure definition pages
            StructureDefinitionHtmlGenerator structureDefinitionGenerator = new StructureDefinitionHtmlGenerator(resourceFileSet, outputPaths);
            structureDefinitionGenerator.GenerateAll();

            // valueset pages
            ValueSetHtmlGenerator valuesetGenerator = new ValueSetHtmlGenerator(resourceFileSet, outputPaths);
            valuesetGenerator.GenerateAll();

            if (showEverythingOnOnePage)
            {
                ResourceListingHtmlGenerator resourceListingGenerator = new ResourceListingHtmlGenerator(outputPaths, showResourceInW5Group, listOnlyResourcesWithMaturity);
                resourceListingGenerator.GenerateSingleResourceListingPageWithIntroText("index.html", resourceFileSet, textContent.IndexPageHtml);
            }
            else
            {
                ResourceListingHtmlGenerator resourceListingGenerator = new ResourceListingHtmlGenerator(outputPaths, showResourceInW5Group, listOnlyResourcesWithMaturity);
                resourceListingGenerator.GenerateStructureDefinitionListing("resources.html", resourceFileSet);

                ResourceListingHtmlGenerator valueSetsListingGenerator = new ResourceListingHtmlGenerator(outputPaths, showResourceInW5Group, listOnlyResourcesWithMaturity);
                resourceListingGenerator.GenerateValueSetListing("valuesets.html", resourceFileSet);

                GenericPageGenerator pageGenerator = new GenericPageGenerator(outputPaths);
                pageGenerator.Generate("index.html", "Overview", textContent.IndexPageHtml);

                pageGenerator.Generate("api.html", "API", GetApiPageContent());
            }

            // profile xml and json files
            SourceFileManager sourceGenerator = new SourceFileManager(outputPaths, resourceFileSet);
            sourceGenerator.CopyXml();
            sourceGenerator.GenerateJson();
            sourceGenerator.CreateRedirectsForProfileUrls();

            return outputPaths.GetOutputPath(OutputFileType.Html, "index.html");
        }

        private string GetApiPageContent()
        {
            return Html.Div(new object[]
            {
                Html.H3("API"),
                Html.P(new object[]
                {
                    "Please see ",
                    Html.A("http://endeavour-cim.cloudapp.net/", "API documentation here"),
                    "."
                })
            }).ToString();
        }
    }
}
