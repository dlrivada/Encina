```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.428 ns | 0.1983 ns | 0.2906 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.233 ns | 0.0092 ns | 0.0135 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.334 ns | 0.2691 ns | 0.3944 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.021 ns | 0.0570 ns | 0.0817 ns |  1.00 | 0.0033 |      56 B |        1.00 |
