```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.640 ns | 7.7161 ns | 0.4229 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.383 ns | 0.0243 ns | 0.0013 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.039 ns | 2.7593 ns | 0.1512 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.813 ns | 3.4516 ns | 0.1892 ns |  1.00 | 0.0033 |      56 B |        1.00 |
