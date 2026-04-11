```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.114 ns | 0.3560 ns | 0.5219 ns |  0.60 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.392 ns | 0.0015 ns | 0.0022 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.004 ns | 0.0817 ns | 0.1172 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.914 ns | 0.1159 ns | 0.1662 ns |  1.00 | 0.0033 |      56 B |        1.00 |
