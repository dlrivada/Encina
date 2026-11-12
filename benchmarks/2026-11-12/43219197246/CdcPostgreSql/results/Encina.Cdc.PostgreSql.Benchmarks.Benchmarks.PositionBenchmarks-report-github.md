```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.334 ns | 0.3522 ns | 0.0193 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.135 ns | 0.5974 ns | 0.0327 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.122 ns | 5.9738 ns | 0.3274 ns |  2.46 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 18.157 ns | 3.8259 ns | 0.2097 ns |  2.96 |    0.03 | 0.0019 |      32 B |        1.33 |
