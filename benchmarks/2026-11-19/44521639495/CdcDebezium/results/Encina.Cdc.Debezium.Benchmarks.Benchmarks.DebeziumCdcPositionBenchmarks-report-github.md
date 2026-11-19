```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  9.345 ns |  5.7298 ns | 0.3141 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.735 ns | 19.5869 ns | 1.0736 ns |  5.33 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.192 ns |  0.3838 ns | 0.0210 ns |  4.30 |    0.13 | 0.0091 |     152 B |        6.33 |
