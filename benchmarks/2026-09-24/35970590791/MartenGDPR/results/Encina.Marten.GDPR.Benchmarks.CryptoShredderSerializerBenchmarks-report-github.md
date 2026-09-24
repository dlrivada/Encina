```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   344.8 ns |    99.66 ns |  5.46 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   380.0 ns |    30.67 ns |  1.68 ns |  1.10 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 4,393.7 ns | 1,085.78 ns | 59.52 ns | 12.75 |    0.23 |    2 | 0.1678 |    2869 B |       10.55 |
