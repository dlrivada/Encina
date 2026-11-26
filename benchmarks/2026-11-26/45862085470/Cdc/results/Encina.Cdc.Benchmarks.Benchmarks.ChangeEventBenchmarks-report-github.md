```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 29.978 ns | 1.3986 ns | 0.0767 ns |  0.85 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.803 ns | 0.1869 ns | 0.0102 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 15.155 ns | 0.9176 ns | 0.0503 ns |  0.43 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.463 ns | 2.2628 ns | 0.1240 ns |  1.00 | 0.0007 |      56 B |        1.00 |
