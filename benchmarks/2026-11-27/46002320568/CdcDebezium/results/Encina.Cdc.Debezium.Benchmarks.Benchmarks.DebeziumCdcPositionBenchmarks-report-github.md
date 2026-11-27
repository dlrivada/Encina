```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.603 ns |  0.8461 ns | 0.0464 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 52.307 ns | 30.5096 ns | 1.6723 ns |  6.88 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.280 ns | 10.5313 ns | 0.5773 ns |  5.56 |    0.07 | 0.0091 |     152 B |        6.33 |
