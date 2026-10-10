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
| CreateChangeEvent          | 23.386 ns | 5.2697 ns | 0.2889 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.386 ns | 0.0085 ns | 0.0005 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.439 ns | 3.3547 ns | 0.1839 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.889 ns | 4.0653 ns | 0.2228 ns |  1.00 | 0.0033 |      56 B |        1.00 |
