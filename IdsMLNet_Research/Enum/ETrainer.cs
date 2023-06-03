using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdsMLNet_Research.Enum
{
    public enum ETrainer
    {
        FastTree,
        SdcaLogisticRegression,
        AveragedPerceptron,
        FastForest,
        FieldAwareFactorizationMachine,
        Gam,
        LbfgsLogisticRegression,
        LdSvm,
        LinearSvm,
        Prior,
        SdcaNonCalibrated,
        SgdNonCalibrated,
        SgdCalibrated
    }
}
