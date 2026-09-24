```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   437.9 ns |  12.73 ns |  0.70 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   470.0 ns |  69.98 ns |  3.84 ns |  1.07 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 6,225.1 ns | 687.25 ns | 37.67 ns | 14.22 |    0.08 |    2 | 0.1678 |    2869 B |       10.55 |
