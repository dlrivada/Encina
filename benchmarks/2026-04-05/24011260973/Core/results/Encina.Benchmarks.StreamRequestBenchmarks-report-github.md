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
| Stream_SmallDataset_10Items             | 65.88 ms |    NA |  1.00 |   17248 B |        1.00 |
| Stream_MediumDataset_100Items           | 66.15 ms |    NA |  1.00 |   35840 B |        2.08 |
| Stream_LargeDataset_1000Items           | 66.88 ms |    NA |  1.02 |  219744 B |       12.74 |
| Stream_WithPipelineBehaviors            | 78.73 ms |    NA |  1.19 |   37552 B |        2.18 |
| Stream_MaterializeToList_100Items       | 67.51 ms |    NA |  1.02 |   45064 B |        2.61 |
| Stream_CountOnly_NoMaterialization      | 66.01 ms |    NA |  1.00 |   35320 B |        2.05 |
| Stream_WithCancellation_EarlyExit       | 69.11 ms |    NA |  1.05 |   52688 B |        3.05 |
| Stream_DirectHandlerInvocation_NoEncina | 14.18 ms |    NA |  0.22 |     400 B |        0.02 |
