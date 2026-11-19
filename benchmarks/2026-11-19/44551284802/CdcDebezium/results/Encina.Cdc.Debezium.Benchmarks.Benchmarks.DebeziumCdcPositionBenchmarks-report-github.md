```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.040 ns |  5.488 ns | 0.3008 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 32.895 ns | 11.771 ns | 0.6452 ns |  6.54 |    0.35 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 23.713 ns | 16.923 ns | 0.9276 ns |  4.72 |    0.29 | 0.0091 |     152 B |        6.33 |
