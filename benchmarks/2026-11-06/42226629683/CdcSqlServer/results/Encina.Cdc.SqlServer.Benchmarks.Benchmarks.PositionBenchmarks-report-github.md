```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.272 ns | 0.0826 ns | 0.0045 ns |  0.22 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  5.896 ns | 0.5932 ns | 0.0325 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 13.346 ns | 6.6472 ns | 0.3644 ns |  2.26 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.052 ns | 3.5806 ns | 0.1963 ns |  1.03 |    0.03 | 0.0019 |      32 B |        1.33 |
