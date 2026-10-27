```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9917 ns | 0.1535 ns | 0.0084 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.2696 ns | 0.7693 ns | 0.0422 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.8539 ns | 1.5472 ns | 0.0848 ns |  1.25 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.7530 ns | 3.7007 ns | 0.2028 ns |  1.24 |    0.03 | 0.0004 |      32 B |        1.33 |
