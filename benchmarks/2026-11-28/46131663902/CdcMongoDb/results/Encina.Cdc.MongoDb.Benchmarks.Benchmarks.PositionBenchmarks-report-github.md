```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.524 ns |  21.7321 ns |  1.1912 ns | 15.96 |    0.14 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.431 ns |   0.7352 ns |  0.0403 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 649.062 ns |  46.0546 ns |  2.5244 ns | 76.98 |    0.41 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 542.696 ns | 339.6157 ns | 18.6155 ns | 64.37 |    1.93 | 0.0601 |    1008 B |       42.00 |
