```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.629 ns | 1.822 ns | 0.0999 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.151 ns | 7.920 ns | 0.4341 ns |  5.76 |    0.09 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.896 ns | 4.397 ns | 0.2410 ns |  4.66 |    0.07 | 0.0091 |     152 B |        6.33 |
