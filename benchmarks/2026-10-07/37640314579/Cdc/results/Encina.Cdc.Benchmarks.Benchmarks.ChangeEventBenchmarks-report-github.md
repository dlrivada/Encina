```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.463 ns | 0.8605 ns | 0.0472 ns |  0.59 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.429 ns | 0.1742 ns | 0.0096 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.921 ns | 3.2132 ns | 0.1761 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.694 ns | 2.7464 ns | 0.1505 ns |  1.00 | 0.0033 |      56 B |        1.00 |
