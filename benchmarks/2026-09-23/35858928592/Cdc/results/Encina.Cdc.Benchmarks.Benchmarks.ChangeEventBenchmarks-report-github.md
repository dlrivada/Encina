```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.738 ns | 3.8019 ns | 0.2084 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.216 ns | 0.0401 ns | 0.0022 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.065 ns | 5.4441 ns | 0.2984 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.355 ns | 1.7352 ns | 0.0951 ns |  1.00 | 0.0033 |      56 B |        1.00 |
