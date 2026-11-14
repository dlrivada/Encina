```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.502 ns |  5.113 ns | 0.2802 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 32.354 ns | 27.159 ns | 1.4887 ns |  7.21 |    0.49 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 19.748 ns |  4.901 ns | 0.2687 ns |  4.40 |    0.25 | 0.0091 |     152 B |        6.33 |
