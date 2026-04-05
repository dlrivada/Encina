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
| Stream_SmallDataset_10Items             | 67.85 ms |    NA |  1.00 |   19528 B |        1.00 |
| Stream_MediumDataset_100Items           | 68.22 ms |    NA |  1.01 |   35320 B |        1.81 |
| Stream_LargeDataset_1000Items           | 68.82 ms |    NA |  1.01 |  230392 B |       11.80 |
| Stream_WithPipelineBehaviors            | 81.52 ms |    NA |  1.20 |   43656 B |        2.24 |
| Stream_MaterializeToList_100Items       | 69.67 ms |    NA |  1.03 |   45136 B |        2.31 |
| Stream_CountOnly_NoMaterialization      | 68.79 ms |    NA |  1.01 |   34144 B |        1.75 |
| Stream_WithCancellation_EarlyExit       | 70.78 ms |    NA |  1.04 |   52832 B |        2.71 |
| Stream_DirectHandlerInvocation_NoEncina | 14.60 ms |    NA |  0.22 |     400 B |        0.02 |
