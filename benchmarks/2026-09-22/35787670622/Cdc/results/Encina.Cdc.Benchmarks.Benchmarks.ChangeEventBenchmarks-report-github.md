```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.032 ns | 2.8865 ns | 0.1582 ns |  0.60 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.502 ns | 0.0293 ns | 0.0016 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.998 ns | 1.6717 ns | 0.0916 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.009 ns | 3.7759 ns | 0.2070 ns |  1.00 | 0.0033 |      56 B |        1.00 |
