```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.295 ns |  0.4109 ns | 0.0225 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.862 ns |  0.6895 ns | 0.0378 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 16.031 ns | 11.3749 ns | 0.6235 ns |  2.34 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.058 ns |  1.6308 ns | 0.0894 ns |  1.03 |    0.01 | 0.0019 |      32 B |        1.33 |
