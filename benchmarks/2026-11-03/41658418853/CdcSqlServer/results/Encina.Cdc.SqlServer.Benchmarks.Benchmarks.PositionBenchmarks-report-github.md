```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9951 ns | 0.1712 ns | 0.0094 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.7492 ns | 5.3638 ns | 0.2940 ns |  1.00 |    0.05 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.6346 ns | 0.3828 ns | 0.0210 ns |  1.28 |    0.05 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.0205 ns | 1.6606 ns | 0.0910 ns |  1.19 |    0.05 | 0.0004 |      32 B |        1.33 |
