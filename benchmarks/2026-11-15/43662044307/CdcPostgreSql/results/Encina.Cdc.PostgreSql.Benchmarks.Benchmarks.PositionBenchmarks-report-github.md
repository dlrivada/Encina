```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.171 ns | 0.0940 ns | 0.1407 ns |  0.19 |    0.02 |      - |         - |        0.00 |
| CreatePosition   |  6.252 ns | 0.1034 ns | 0.1450 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 15.857 ns | 0.4478 ns | 0.6703 ns |  2.54 |    0.12 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 17.693 ns | 0.4973 ns | 0.7443 ns |  2.83 |    0.13 | 0.0019 |      32 B |        1.33 |
