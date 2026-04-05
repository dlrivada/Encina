```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                  | Mean     | Error | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|------:|----------:|------------:|
| Stream_SmallDataset_10Items             | 66.25 ms |    NA |  1.00 |   17112 B |        1.00 |
| Stream_MediumDataset_100Items           | 65.90 ms |    NA |  0.99 |   35248 B |        2.06 |
| Stream_LargeDataset_1000Items           | 66.67 ms |    NA |  1.01 |  222824 B |       13.02 |
| Stream_WithPipelineBehaviors            | 78.77 ms |    NA |  1.19 |   37680 B |        2.20 |
| Stream_MaterializeToList_100Items       | 67.29 ms |    NA |  1.02 |   50944 B |        2.98 |
| Stream_CountOnly_NoMaterialization      | 66.30 ms |    NA |  1.00 |   35248 B |        2.06 |
| Stream_WithCancellation_EarlyExit       | 68.79 ms |    NA |  1.04 |   49208 B |        2.88 |
| Stream_DirectHandlerInvocation_NoEncina | 14.44 ms |    NA |  0.22 |     400 B |        0.02 |
