```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 19.128 ns | 7.9665 ns | 0.4367 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.024 ns | 0.3818 ns | 0.0209 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.685 ns | 5.1009 ns | 0.2796 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.961 ns | 5.6977 ns | 0.3123 ns |  1.00 | 0.0033 |      56 B |        1.00 |
