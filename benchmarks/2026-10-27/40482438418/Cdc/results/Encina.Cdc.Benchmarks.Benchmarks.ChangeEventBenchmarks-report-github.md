```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 28.082 ns | 2.5380 ns | 0.1391 ns |  0.81 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.816 ns | 0.1792 ns | 0.0098 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.955 ns | 2.6597 ns | 0.1458 ns |  0.43 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.714 ns | 2.5501 ns | 0.1398 ns |  1.00 | 0.0007 |      56 B |        1.00 |
