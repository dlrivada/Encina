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
| Stream_SmallDataset_10Items             | 68.77 ms |    NA |  1.00 |   17248 B |        1.00 |
| Stream_MediumDataset_100Items           | 68.29 ms |    NA |  0.99 |   35320 B |        2.05 |
| Stream_LargeDataset_1000Items           | 68.33 ms |    NA |  0.99 |  222824 B |       12.92 |
| Stream_WithPipelineBehaviors            | 81.01 ms |    NA |  1.18 |   43688 B |        2.53 |
| Stream_MaterializeToList_100Items       | 69.59 ms |    NA |  1.01 |   52640 B |        3.05 |
| Stream_CountOnly_NoMaterialization      | 68.56 ms |    NA |  1.00 |   36072 B |        2.09 |
| Stream_WithCancellation_EarlyExit       | 70.92 ms |    NA |  1.03 |   51504 B |        2.99 |
| Stream_DirectHandlerInvocation_NoEncina | 14.71 ms |    NA |  0.21 |     400 B |        0.02 |
