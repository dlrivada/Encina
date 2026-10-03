```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   359.2 ns | 200.31 ns | 10.98 ns |  1.00 |    0.04 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   366.3 ns |  55.15 ns |  3.02 ns |  1.02 |    0.03 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 5,128.1 ns |  35.40 ns |  1.94 ns | 14.29 |    0.37 |    2 | 0.0305 |    2637 B |        9.69 |
