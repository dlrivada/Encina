```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   368.2 ns |   291.3 ns | 15.97 ns |  1.00 |    0.05 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   378.8 ns |   128.4 ns |  7.04 ns |  1.03 |    0.04 |    1 | 0.0029 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 5,669.3 ns | 1,097.9 ns | 60.18 ns | 15.42 |    0.58 |    2 | 0.0305 |    2869 B |       10.55 |
