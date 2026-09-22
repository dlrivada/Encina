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
| CreateChangeEvent          | 21.103 ns | 6.5286 ns | 0.3579 ns |  0.60 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.385 ns | 0.0202 ns | 0.0011 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.127 ns | 3.0396 ns | 0.1666 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.900 ns | 4.3482 ns | 0.2383 ns |  1.00 | 0.0033 |      56 B |        1.00 |
