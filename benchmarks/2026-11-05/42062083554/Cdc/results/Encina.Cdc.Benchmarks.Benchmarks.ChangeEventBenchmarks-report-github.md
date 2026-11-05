```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.403 ns | 8.2837 ns | 0.4541 ns |  0.68 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.380 ns | 0.0233 ns | 0.0013 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.172 ns | 4.8666 ns | 0.2668 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.118 ns | 3.1959 ns | 0.1752 ns |  1.00 | 0.0033 |      56 B |        1.00 |
