```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5091 ns | 1.1755 ns | 0.0644 ns |  0.15 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 3.2938 ns | 0.8688 ns | 0.0476 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8174 ns | 2.7313 ns | 0.1497 ns |  1.16 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3199 ns | 1.1309 ns | 0.0620 ns |  1.01 |    0.02 | 0.0019 |      32 B |        1.33 |
