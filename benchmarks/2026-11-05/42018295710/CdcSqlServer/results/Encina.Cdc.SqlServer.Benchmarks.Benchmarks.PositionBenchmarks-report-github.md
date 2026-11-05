```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.296 ns |  0.5065 ns | 0.0278 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.157 ns |  0.9232 ns | 0.0506 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.315 ns | 21.6310 ns | 1.1857 ns |  2.49 |    0.17 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.833 ns | 37.2325 ns | 2.0408 ns |  2.90 |    0.29 | 0.0019 |      32 B |        1.33 |
