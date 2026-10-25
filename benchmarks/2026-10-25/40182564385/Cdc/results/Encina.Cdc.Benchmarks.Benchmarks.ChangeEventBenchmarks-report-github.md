```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.673 ns | 6.2079 ns | 0.3403 ns |  0.67 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.644 ns | 0.3584 ns | 0.0196 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.553 ns | 6.3806 ns | 0.3497 ns |  0.37 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.071 ns | 2.4921 ns | 0.1366 ns |  1.00 | 0.0007 |      56 B |        1.00 |
