```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.117 ns | 0.0411 ns | 0.0603 ns | 1.073 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.067 ns | 0.1085 ns | 0.1590 ns | 5.092 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.060 ns | 0.1348 ns | 0.2018 ns | 6.025 ns |  1.20 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.771 ns | 0.1078 ns | 0.1547 ns | 5.709 ns |  1.14 |    0.05 | 0.0019 |      32 B |        1.33 |
