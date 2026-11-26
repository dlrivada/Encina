```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.326 ns | 0.2222 ns | 0.0122 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   |  6.345 ns | 1.5186 ns | 0.0832 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 14.229 ns | 7.8176 ns | 0.4285 ns |  2.24 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 16.629 ns | 9.1128 ns | 0.4995 ns |  2.62 |    0.07 | 0.0019 |      32 B |        1.33 |
