```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.147 ns | 4.9925 ns | 0.2737 ns |  0.70 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.390 ns | 0.0608 ns | 0.0033 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.095 ns | 0.6945 ns | 0.0381 ns |  0.39 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.005 ns | 0.4382 ns | 0.0240 ns |  1.00 | 0.0033 |      56 B |        1.00 |
