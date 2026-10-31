```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9819 ns | 0.0537 ns | 0.0029 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.6426 ns | 2.0704 ns | 0.1135 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.5116 ns | 0.7033 ns | 0.0385 ns |  1.28 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.4835 ns | 0.6145 ns | 0.0337 ns |  1.13 |    0.02 | 0.0004 |      32 B |        1.33 |
