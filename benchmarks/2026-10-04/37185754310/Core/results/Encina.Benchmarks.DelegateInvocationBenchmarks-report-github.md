```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.70 ns |     3.306 ns |   0.181 ns |     1.25 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     22.94 ns |     4.055 ns |   0.222 ns |     1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 23,859.70 ns | 4,129.121 ns | 226.331 ns | 1,040.35 |   12.19 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     95.52 ns |    23.933 ns |   1.312 ns |     4.16 |    0.06 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     69.45 ns |    69.773 ns |   3.825 ns |     3.03 |    0.15 | 0.0020 |      - |     176 B |        1.57 |
