```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.745 ns | 2.7760 ns | 0.1522 ns |  0.59 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.381 ns | 0.0169 ns | 0.0009 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.237 ns | 3.3076 ns | 0.1813 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.757 ns | 1.1238 ns | 0.0616 ns |  1.00 | 0.0033 |      56 B |        1.00 |
