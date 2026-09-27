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
| CreateChangeEvent          | 21.429 ns | 7.3695 ns | 0.4039 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.388 ns | 0.0638 ns | 0.0035 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.945 ns | 2.1825 ns | 0.1196 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.795 ns | 4.3116 ns | 0.2363 ns |  1.00 | 0.0033 |      56 B |        1.00 |
