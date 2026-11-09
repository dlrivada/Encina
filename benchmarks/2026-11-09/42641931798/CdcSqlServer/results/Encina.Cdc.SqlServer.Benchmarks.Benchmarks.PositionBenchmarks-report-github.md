```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.278 ns |  0.0740 ns | 0.0041 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.226 ns |  0.7891 ns | 0.0433 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.988 ns | 17.3104 ns | 0.9488 ns |  2.57 |    0.13 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.025 ns |  0.8072 ns | 0.0442 ns |  0.97 |    0.01 | 0.0019 |      32 B |        1.33 |
