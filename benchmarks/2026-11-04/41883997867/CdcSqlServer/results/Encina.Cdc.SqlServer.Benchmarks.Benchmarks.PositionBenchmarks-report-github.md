```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.280 ns |  0.1859 ns | 0.0102 ns |  0.19 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.686 ns |  2.6523 ns | 0.1454 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.039 ns | 10.7108 ns | 0.5871 ns |  2.25 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.631 ns |  1.9641 ns | 0.1077 ns |  0.99 |    0.02 | 0.0019 |      32 B |        1.33 |
