```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.317 ns | 0.0768 ns | 0.0042 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   |  9.089 ns | 6.5078 ns | 0.3567 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.066 ns | 2.4013 ns | 0.1316 ns |  0.78 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 10.122 ns | 3.9404 ns | 0.2160 ns |  1.11 |    0.04 | 0.0019 |      32 B |        1.33 |
