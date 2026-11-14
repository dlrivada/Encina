```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.669 ns | 0.5226 ns | 0.0286 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 27.410 ns | 3.7335 ns | 0.2046 ns |  5.87 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.924 ns | 2.1933 ns | 0.1202 ns |  4.48 |    0.03 | 0.0091 |     152 B |        6.33 |
