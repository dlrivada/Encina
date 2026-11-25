```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.334 ns |  0.9142 ns | 0.0501 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 39.071 ns | 12.2580 ns | 0.6719 ns |  6.17 |    0.10 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.728 ns | 14.2295 ns | 0.7800 ns |  5.01 |    0.11 | 0.0091 |     152 B |        6.33 |
