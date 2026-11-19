```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.546 ns |  1.536 ns | 0.0842 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 41.748 ns | 21.345 ns | 1.1700 ns |  5.53 |    0.14 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 32.207 ns |  5.065 ns | 0.2777 ns |  4.27 |    0.05 | 0.0091 |     152 B |        6.33 |
