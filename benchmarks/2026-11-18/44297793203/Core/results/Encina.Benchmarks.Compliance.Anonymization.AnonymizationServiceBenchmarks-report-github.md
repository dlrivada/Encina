```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,434.1 ns |  2,676.45 ns |   146.71 ns |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4090 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,320.8 ns |  2,264.90 ns |   124.15 ns |  0.99 |    0.02 |    5 |  0.2289 | 0.0763 |    4170 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,162.5 ns |    535.93 ns |    29.38 ns |  0.26 |    0.00 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,160.4 ns |    190.77 ns |    10.46 ns |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,700.5 ns |     24.41 ns |     1.34 ns |  0.44 |    0.01 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     784.3 ns |    485.95 ns |    26.64 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 194,095.7 ns | 29,643.66 ns | 1,624.87 ns | 23.02 |    0.38 |    6 | 17.3340 | 0.4883 |  291296 B |       71.22 |
