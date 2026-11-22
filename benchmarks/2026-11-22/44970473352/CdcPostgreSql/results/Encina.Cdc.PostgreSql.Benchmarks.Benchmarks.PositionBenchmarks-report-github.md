```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.314 ns | 0.0126 ns | 0.0185 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  6.400 ns | 0.1049 ns | 0.1570 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.395 ns | 0.4448 ns | 0.6657 ns |  2.41 |    0.12 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 16.650 ns | 0.7589 ns | 1.1123 ns |  2.60 |    0.18 | 0.0019 |      32 B |        1.33 |
