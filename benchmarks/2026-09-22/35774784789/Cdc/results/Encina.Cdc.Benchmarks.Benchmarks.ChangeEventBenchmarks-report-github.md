```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 17.1905 ns | 1.9578 ns | 0.1073 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9919 ns | 0.1792 ns | 0.0098 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.1588 ns | 4.3602 ns | 0.2390 ns |  0.40 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.7146 ns | 2.0921 ns | 0.1147 ns |  1.00 | 0.0033 |      56 B |        1.00 |
