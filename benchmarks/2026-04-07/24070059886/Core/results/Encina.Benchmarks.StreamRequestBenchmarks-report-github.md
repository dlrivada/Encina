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
| Stream_SmallDataset_10Items             | 67.17 ms |    NA |  1.00 |   17288 B |        1.00 |
| Stream_MediumDataset_100Items           | 67.64 ms |    NA |  1.01 |   35320 B |        2.04 |
| Stream_LargeDataset_1000Items           | 67.40 ms |    NA |  1.00 |  230392 B |       13.33 |
| Stream_WithPipelineBehaviors            | 80.38 ms |    NA |  1.20 |   41976 B |        2.43 |
| Stream_MaterializeToList_100Items       | 68.75 ms |    NA |  1.02 |   48784 B |        2.82 |
| Stream_CountOnly_NoMaterialization      | 67.73 ms |    NA |  1.01 |   35248 B |        2.04 |
| Stream_WithCancellation_EarlyExit       | 70.79 ms |    NA |  1.05 |   47072 B |        2.72 |
| Stream_DirectHandlerInvocation_NoEncina | 14.50 ms |    NA |  0.22 |     400 B |        0.02 |
