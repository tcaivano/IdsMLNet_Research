using IdsMLNet_Research.Data;
using IdsMLNet_Research.Enum;
using IdsMLNet_Research.Services;
using System.Text;

const ESampleStrategy _preprocessEventStrategy = ESampleStrategy.CompleteOnly;
const string _authFileLocation = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\auth.txt";
const string _redTeamFileLocation = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\redteam.txt";
const string _truthFileLocation = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\truth.txt";
const string _finalTestDataLocation = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\test.txt";
const string _truthFileLocationRandomUpSample = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\truthRandomUpSample.txt";
const string _truthFileLocationRandomDownSample = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\truthRandomDownSample.txt";
const string _truthFileRedOnly = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\truthRedOnly.txt";
const string _truthFile89 = @"C:\Users\itali\source\repos\IdsMLNet_Research\IdsMLNet_Research\Data\truth89.txt";


LargeParserService.CreateBaseTruth(_authFileLocation, _redTeamFileLocation, _truthFileLocation, _finalTestDataLocation);

LargeParserService.CreateSampledDataSet(_truthFileLocation, ESampleStrategy.All, _truthFileRedOnly, _truthFile89, _truthFileLocationRandomUpSample, _truthFileLocationRandomDownSample);