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
| CreateChangeEvent          | 21.032 ns | 2.3235 ns | 0.1274 ns |  0.61 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.386 ns | 0.0023 ns | 0.0001 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.651 ns | 0.9627 ns | 0.0528 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.439 ns | 0.4577 ns | 0.0251 ns |  1.00 | 0.0033 |      56 B |        1.00 |
